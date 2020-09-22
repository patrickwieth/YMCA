

Trigger.OnAnyProduction(
  function(producer, produced, productionType)

    if produced.IsTaggable
    then
      Trigger.OnPassengerEntered(produced,
        function(transport, passenger)
          if passenger.Type == 'shell.ap' and (not transport.HasTag('shell'))
          then
            Media.DisplayMessage('unload')
            transport.UnloadPassengers()
          end
        end
      )
      Trigger.OnPassengerEntered(produced,
        function(transport, passenger)
          if passenger.Type == 'double_turret'
          then
            Media.DisplayMessage('add tag shell')
            transport.AddTag('shell')
          end
        end
      )
      Trigger.OnPassengerExited(produced,
        function(transport, passenger)
          if passenger.Type == 'double_turret'
          then
            transport.RemoveTag('shell')
            Media.DisplayMessage('remove tag shell')
          end
        end
      )
    end
  end
)
